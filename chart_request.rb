# frozen_string_literal: true
class ChartRequest
  attr_accessor :method, :chart_params
  DEFAULT = {}.freeze

  def initialize(method, params = {})
    @method = method
    @chart_params = DEFAULT.merge(params)
    @chart_params.symbolize_keys!
  end

  def url
    URI.parse(
      "http://#{Rails.configuration.inquicker.iqetl.hostname}:#{Rails.configuration.inquicker.iqetl.port}/#{chart_path}?#{query_params}"
    )
  end

  def results_name
    @chart_params[:as] || @method
  end

  def query_params
    params = {}

    @date_range = parse_date_range(@chart_params[:date_range])

    params[:start_date] = @date_range.begin
    params[:end_date] = @date_range.end

    @chart_params[:ed] = @chart_params[:er] if @chart_params[:er].present?

    additional_keys.each do |key|
      next unless @chart_params.key?(key)
      params[key] = 1
    end

    if @chart_params[:inactive_facilities]
      params[:inactive_facilities] = @chart_params[:inactive_facilities]
    end

    if @chart_params[:date_filter_type]
      params[:date_filter_type] = @chart_params[:date_filter_type]
    end

    if @chart_params[:exclude_cancel].present?
      params[:exclude_cancel] = @chart_params[:exclude_cancel]
    else
      params[:exclude_cancel] = "off"
    end

    chartable_resources.each do | t |
      next unless val = @chart_params[t]
      params[t] = case val
        when Array; val.join(',')
        else; val
      end
    end

    URI.encode_www_form(params)
  end

  def parse_date_range(str)
    if @chart_params[:start_date] && @chart_params[:end_date]
      @chart_params[:start_date]..@chart_params[:end_date]
    else
      dates = str.split(' - ')
      (dates.first)..(dates.last)
    end
  rescue
    days_to_display = @chart_params.fetch(:default_days_to_display, 30).days.ago.to_date
    days_to_display..Date.current
  end

  protected

  def additional_keys
    %i(mobile ed pcp ucc inactive).freeze
  end

  def chartable_resources
    %i(health_systems facilities locations providers regions state).freeze
  end

  def chart_path
    if @chart_params[:facility_id].present?
      @facility ||= Facility.find_by(id: @chart_params[:facility_id])
    elsif @chart_params[:health_system_id].present?
      @health_system ||= HealthSystem.find_by(id: @chart_params[:health_system_id])
    end

    if @facility
      URI.escape("#{method}/#{@facility.health_system.id}/#{@facility.id}")
    elsif @health_system
      URI.escape("#{method}/#{@health_system.id}")
    else
      URI.escape("#{method}")
    end
  end
end
